import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { sortDirections } from 'Helpers/Props';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import BookRowConnector from './BookRowConnector';
import styles from './AuthorDetailsSeason.css';

class AuthorDetailsSeason extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.setSelectedState(this.props.items);
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection,
      setSelectedState
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      setSelectedState(items);
    }
  }

  //
  // Listeners

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    const {
      onSelectedChange,
      items
    } = this.props;

    return onSelectedChange(items, id, value, shiftKey);
  };

  //
  // Render

  render() {
    const {
      items,
      isEditorActive,
      columns,
      sortKey,
      sortDirection,
      onSortPress,
      onTableOptionChange,
      selectedState
    } = this.props;

    let titleColumns = columns.filter((x) => x.name !== 'monitored');
    if (!isEditorActive) {
      titleColumns = titleColumns.filter((x) => x.name !== 'select');
    }

    return (
      <div
        className={styles.bookType}
      >
        <div className={styles.books}>
          <Table
            columns={titleColumns}
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
                      columns={titleColumns}
                      {...item}
                      isEditorActive={isEditorActive}
                      isSelected={selectedState[item.id]}
                      onSelectedChange={this.onSelectedChange}
                    />
                  );
                })
              }
            </TableBody>
          </Table>
        </div>
      </div>
    );
  }
}

AuthorDetailsSeason.propTypes = {
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  setSelectedState: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired
};

export default AuthorDetailsSeason;
