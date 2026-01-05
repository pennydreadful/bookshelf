import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import BookRowConnector from './BookRowConnector';
import styles from './AuthorDetailsSeries.css';

class AuthorDetailsSeries extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isManageBooksOpen: false
    };
  }

  componentDidMount() {
    this._expandByDefault();
  }

  componentDidUpdate(prevProps) {
    const {
      authorId
    } = this.props;

    if (prevProps.authorId !== authorId) {
      this._expandByDefault();
      return;
    }
  }

  //
  // Control

  _expandByDefault() {
    const {
      id,
      onExpandPress
    } = this.props;

    onExpandPress(id, true);
  }

  //
  // Listeners

  onExpandPress = () => {
    const {
      id,
      isExpanded
    } = this.props;

    this.props.onExpandPress(id, !isExpanded);
  };

  //
  // Render

  render() {
    const {
      label,
      items,
      positionMap,
      columns,
      isExpanded,
      sortKey,
      sortDirection,
      onSortPress,
      isSmallScreen,
      onTableOptionChange
    } = this.props;

    const tableColumns = columns.filter((column) => column.name !== 'monitored');

    return (
      <div
        className={styles.bookType}
      >
        <div className={styles.seriesTitle}>
          <Link
            className={styles.expandButton}
            onPress={this.onExpandPress}
          >
            <div className={styles.header}>
              <div className={styles.left}>
                {
                  <div>
                    <span className={styles.bookTypeLabel}>
                      {label}
                    </span>

                    <span className={styles.bookCount}>
                      ({items.length} Books)
                    </span>
                  </div>
                }

              </div>

              <Icon
                className={styles.expandButtonIcon}
                name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
                title={isExpanded ? translate('IsExpandedHideBooks') : translate('IsExpandedShowBooks')}
                size={24}
              />

              {
                !isSmallScreen &&
                  <span>&nbsp;</span>
              }

            </div>
          </Link>
        </div>

        <div>
          {
            isExpanded &&
              <div className={styles.books}>
                <Table
                  columns={tableColumns}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  onSortPress={onSortPress}
                  onTableOptionChange={onTableOptionChange}
                >
                  <TableBody>
                    {
                      items.map((item) => {
                        return (
                          <BookRowConnector
                            key={item.id}
                            columns={tableColumns}
                            {...item}
                            position={positionMap[item.id]}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>

                <div className={styles.collapseButtonContainer}>
                  <IconButton
                    iconClassName={styles.collapseButtonIcon}
                    name={icons.COLLAPSE}
                    size={20}
                    title={translate('HideBooks')}
                    onPress={this.onExpandPress}
                  />
                </div>
              </div>
          }
        </div>
      </div>
    );
  }
}

AuthorDetailsSeries.propTypes = {
  id: PropTypes.number.isRequired,
  authorId: PropTypes.number.isRequired,
  label: PropTypes.string.isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  positionMap: PropTypes.object.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isExpanded: PropTypes.bool,
  isSmallScreen: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired
};

export default AuthorDetailsSeries;
